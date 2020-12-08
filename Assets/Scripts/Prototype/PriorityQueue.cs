using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class PriorityQueue
{
    public class Node
    {
        public MaterialStructureGrid.Node data;
        // Lower values indicate 
        // higher priority 
        public float priority;

        public Node next;
    }

    public static Node node = new Node();

    // Function to Create A New Node 
    public static Node newNode(MaterialStructureGrid.Node d, float p)
    {
        Node temp = new Node();
        temp.data = d;
        temp.priority = p;
        temp.next = null;

        return temp;
    }

    // Return the value at head 
    public static MaterialStructureGrid.Node peek(Node head)
    {
        return (head).data;
    }

    // Removes the element with the 
    // highest priority form the list 
    public static Node pop(Node head)
    {
        Node temp = head;
        (head) = (head).next;
        return head;
    }

    // Function to push according to priority 
    public static Node push(Node head,
                            MaterialStructureGrid.Node d, float p)
    {
        Node start = (head);

        // Create new Node 
        Node temp = newNode(d, p);

        // Special Case: The head of list 
        // has lesser priority than new node. 
        // So insert new node before head node 
        // and change head node. 
        if ((head).priority > p)
        {
            // Insert New Node before head 
            temp.next = head;
            (head) = temp;
        }
        else
        {

            // Traverse the list and find a 
            // position to insert new node 
            while (start.next != null &&
                start.next.priority < p)
            {
                start = start.next;
            }

            // Either at the ends of the list 
            // or at required position 
            temp.next = start.next;
            start.next = temp;
        }
        return head;
    }

    // Function to check is list is empty 
    public static int isEmpty(Node head)
    {
        return ((head) == null) ? 1 : 0;
    }

    // Driver code 
    /*public static void Main(string[] args)
    {
        // Create a Priority Queue 
        // 7.4.5.6 
        Node pq = newNode(4, 1);
        pq = push(pq, 5, 2);
        pq = push(pq, 6, 3);
        pq = push(pq, 7, 0);

        while (isEmpty(pq) == 0)
        {
            Debug.Log("{0:D} "+ peek(pq));
            pq = pop(pq);
        }
    }*/
} 

// This code is contributed by Shrikant13 
